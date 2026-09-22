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
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,168.3 ns | 2,061.6 ns | 113.01 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   197.0 ns |   426.7 ns |  23.39 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,831.0 ns |   904.3 ns |  49.57 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   171.0 ns |   632.0 ns |  34.64 ns |         - |
| &#39;Read HasIntent&#39;                       |   433.7 ns | 3,568.1 ns | 195.58 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   231.0 ns |   364.9 ns |  20.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   261.0 ns |   795.2 ns |  43.59 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,666.8 ns | 3,024.8 ns | 165.80 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,499.7 ns | 1,562.0 ns |  85.62 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,273.7 ns | 2,538.5 ns | 139.14 ns |      96 B |
