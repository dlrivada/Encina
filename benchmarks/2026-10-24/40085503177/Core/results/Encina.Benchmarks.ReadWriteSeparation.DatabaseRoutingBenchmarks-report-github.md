```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,297.0 ns | 29,887.7 ns | 1,638.25 ns | 3,749.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   275.3 ns |  1,989.3 ns |   109.04 ns |   234.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 5,429.3 ns | 24,004.9 ns | 1,315.79 ns | 4,984.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   274.8 ns |  3,701.3 ns |   202.88 ns |   172.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   196.3 ns |  1,443.1 ns |    79.10 ns |   241.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   763.7 ns | 19,597.0 ns | 1,074.18 ns |   150.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   | 1,427.7 ns | 13,359.7 ns |   732.29 ns | 1,816.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,304.7 ns | 23,551.9 ns | 1,290.96 ns | 2,852.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 5,196.7 ns | 27,694.1 ns | 1,518.01 ns | 4,642.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,644.7 ns | 19,887.5 ns | 1,090.10 ns | 3,662.0 ns |      96 B |
