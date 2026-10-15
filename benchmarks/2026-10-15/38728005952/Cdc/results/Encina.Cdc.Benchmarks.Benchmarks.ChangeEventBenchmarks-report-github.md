```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.723 ns | 10.5311 ns | 0.5772 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.378 ns |  0.0166 ns | 0.0009 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.927 ns |  2.8389 ns | 0.1556 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.673 ns |  4.8855 ns | 0.2678 ns |  1.00 | 0.0033 |      56 B |        1.00 |
