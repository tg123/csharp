using k8s;
using k8s.Models;
using System;
using System.Net;
using System.Threading.Tasks;

void ListNamespaces(IKubernetes client)
{
    var list = client.CoreV1.ListNamespace();
    foreach (var item in list.Items)
    {
        Console.WriteLine(item.Metadata.Name);
    }

    if (list.Items.Count == 0)
    {
        Console.WriteLine("Empty!");
    }
}

async Task DeleteAsync(IKubernetes client, string name, int delayMillis)
{
    while (true)
    {
        await Task.Delay(delayMillis).ConfigureAwait(false);
        try
        {
            await client.CoreV1.ReadNamespaceAsync(name).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                if (innerEx is k8s.Autorest.HttpOperationException exception)
                {
                    var code = exception.Response.StatusCode;
                    if (code == HttpStatusCode.NotFound)
                    {
                        return;
                    }

                    throw;
                }
            }
        }
        catch (k8s.Autorest.HttpOperationException ex)
        {
            if (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            throw;
        }
    }
}

void Delete(IKubernetes client, string name, int delayMillis)
{
    DeleteAsync(client, name, delayMillis).Wait();
}

var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
IKubernetes client = new Kubernetes(k8SClientConfig);

ListNamespaces(client);

var ns = new V1Namespace { Metadata = new V1ObjectMeta { Name = "test" } };

var result = client.CoreV1.CreateNamespace(ns);
Console.WriteLine(result);

ListNamespaces(client);

var status = client.CoreV1.DeleteNamespace(ns.Metadata.Name, new V1DeleteOptions());

if (status.HasObject)
{
    var obj = status.ObjectView<V1Namespace>();
    Console.WriteLine(obj.Status.Phase);

    Delete(client, ns.Metadata.Name, 3 * 1000);
}
else
{
    Console.WriteLine(status.Message);
}

ListNamespaces(client);
