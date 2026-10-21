```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.753 ns | 4.9108 ns | 0.2692 ns |  0.68 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.394 ns | 0.3323 ns | 0.0182 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.897 ns | 5.2930 ns | 0.2901 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.455 ns | 4.3104 ns | 0.2363 ns |  1.00 | 0.0033 |      56 B |        1.00 |
