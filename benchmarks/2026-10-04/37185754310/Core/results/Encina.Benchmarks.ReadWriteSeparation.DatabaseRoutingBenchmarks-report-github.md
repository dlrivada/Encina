```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,198.3 ns | 3,217.3 ns | 176.35 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   176.0 ns | 1,109.7 ns |  60.83 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,510.7 ns | 4,351.4 ns | 238.52 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   233.0 ns | 1,075.8 ns |  58.97 ns |         - |
| &#39;Read HasIntent&#39;                       |   297.2 ns | 1,695.1 ns |  92.92 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   229.0 ns | 1,867.4 ns | 102.36 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   263.3 ns |   822.7 ns |  45.09 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,604.0 ns | 4,130.0 ns | 226.38 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,666.3 ns | 4,141.8 ns | 227.02 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,223.7 ns |   846.0 ns |  46.37 ns |      96 B |
