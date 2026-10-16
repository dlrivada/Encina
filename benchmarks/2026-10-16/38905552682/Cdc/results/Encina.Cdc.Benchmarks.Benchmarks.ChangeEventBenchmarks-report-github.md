```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 26.867 ns | 7.6143 ns | 0.4174 ns |  0.77 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.731 ns | 0.8710 ns | 0.0477 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.963 ns | 2.7572 ns | 0.1511 ns |  0.43 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.798 ns | 4.5483 ns | 0.2493 ns |  1.00 | 0.0007 |      56 B |        1.00 |
