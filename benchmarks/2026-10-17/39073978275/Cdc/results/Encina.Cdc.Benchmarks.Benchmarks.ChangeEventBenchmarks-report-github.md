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
| CreateChangeEvent          | 28.779 ns | 2.7871 ns | 0.1528 ns |  0.77 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.387 ns | 0.0214 ns | 0.0012 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.727 ns | 2.4861 ns | 0.1363 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 37.401 ns | 0.8980 ns | 0.0492 ns |  1.00 | 0.0033 |      56 B |        1.00 |
