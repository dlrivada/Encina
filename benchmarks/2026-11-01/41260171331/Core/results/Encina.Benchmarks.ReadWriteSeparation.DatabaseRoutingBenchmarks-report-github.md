```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev      | Allocated |
|--------------------------------------- |------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,642.63 ns | 736.35 ns | 1,032.25 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    51.32 ns |  13.53 ns |    18.06 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,095.10 ns | 107.37 ns |   146.97 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   104.04 ns |  26.06 ns |    35.67 ns |         - |
| &#39;Read HasIntent&#39;                       |   103.64 ns |  20.27 ns |    29.07 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    79.18 ns |  53.19 ns |    76.28 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    80.36 ns |  11.19 ns |    14.94 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,370.33 ns | 675.27 ns |   989.80 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,458.04 ns | 556.84 ns |   798.60 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,181.07 ns | 563.60 ns |   826.12 ns |      96 B |
