```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,472.3 ns |  8,874.1 ns |   486.42 ns | 4,449.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   224.3 ns |  1,069.0 ns |    58.59 ns |   201.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,845.5 ns |  1,906.2 ns |   104.48 ns | 3,812.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   322.7 ns |  2,656.0 ns |   145.58 ns |   396.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   297.7 ns |    640.7 ns |    35.12 ns |   301.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    | 1,135.0 ns | 28,123.3 ns | 1,541.53 ns |   250.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   242.2 ns |    862.2 ns |    47.26 ns |   225.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,368.7 ns |  7,017.8 ns |   384.67 ns | 4,199.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,948.7 ns |  7,803.0 ns |   427.71 ns | 3,722.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,300.3 ns |  2,084.9 ns |   114.28 ns | 2,334.0 ns |      96 B |
