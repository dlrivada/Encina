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
| CreateChangeEvent          | 22.668 ns | 2.3089 ns | 0.1266 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.381 ns | 0.0216 ns | 0.0012 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.243 ns | 3.4607 ns | 0.1897 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.264 ns | 2.6724 ns | 0.1465 ns |  1.00 | 0.0033 |      56 B |        1.00 |
