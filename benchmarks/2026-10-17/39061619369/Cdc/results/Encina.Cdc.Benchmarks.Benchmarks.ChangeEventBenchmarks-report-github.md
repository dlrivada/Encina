```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.7625 ns | 3.9919 ns | 0.2188 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9821 ns | 0.0184 ns | 0.0010 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.9761 ns | 4.1671 ns | 0.2284 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.3741 ns | 1.1388 ns | 0.0624 ns |  1.00 | 0.0033 |      56 B |        1.00 |
