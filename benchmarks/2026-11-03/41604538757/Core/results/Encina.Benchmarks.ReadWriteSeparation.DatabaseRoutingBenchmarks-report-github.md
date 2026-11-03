```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,256.5 ns | 23,846.6 ns | 1,307.11 ns | 5,236.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   138.8 ns |  2,044.8 ns |   112.08 ns |    98.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 5,368.3 ns | 21,887.8 ns | 1,199.74 ns | 4,787.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   163.7 ns |  1,604.7 ns |    87.96 ns |   174.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   224.7 ns |  1,005.0 ns |    55.08 ns |   242.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   242.5 ns |  1,781.2 ns |    97.63 ns |   216.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   161.7 ns |  1,842.7 ns |   101.01 ns |   163.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,470.8 ns | 15,711.1 ns |   861.18 ns | 3,911.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,091.8 ns | 46,660.0 ns | 2,557.59 ns | 3,771.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 4,018.7 ns | 23,359.7 ns | 1,280.42 ns | 3,389.00 ns |      96 B |
