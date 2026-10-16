```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,154.3 ns |  2,566.5 ns |   140.68 ns | 4,198.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   198.8 ns |    899.9 ns |    49.33 ns |   175.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,846.3 ns |  9,772.1 ns |   535.64 ns | 3,626.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   249.7 ns |  1,281.0 ns |    70.22 ns |   219.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   206.0 ns |    460.5 ns |    25.24 ns |   210.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   224.0 ns |    111.0 ns |     6.08 ns |   221.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,051.8 ns | 26,427.5 ns | 1,448.58 ns |   220.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,579.3 ns |  4,379.8 ns |   240.07 ns | 3,456.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,425.7 ns |  3,668.5 ns |   201.08 ns | 4,369.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,936.0 ns |  3,706.7 ns |   203.18 ns | 3,016.0 ns |      96 B |
