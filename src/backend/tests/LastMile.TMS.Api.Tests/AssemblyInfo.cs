using Xunit;
// WebApplicationFactory + top-level Program: parallel fixture startup races HostFactoryResolver.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
