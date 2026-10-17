```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.435 ns | 9.0856 ns | 0.4980 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.381 ns | 0.0290 ns | 0.0016 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.482 ns | 4.6385 ns | 0.2542 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.275 ns | 1.4527 ns | 0.0796 ns |  1.00 | 0.0033 |      56 B |        1.00 |
