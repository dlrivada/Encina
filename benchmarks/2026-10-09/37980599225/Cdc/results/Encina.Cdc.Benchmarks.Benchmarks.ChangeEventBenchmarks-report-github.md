```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.904 ns | 12.9853 ns | 0.7118 ns |  0.63 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.382 ns |  0.0269 ns | 0.0015 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.162 ns |  1.0472 ns | 0.0574 ns |  0.36 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.119 ns | 11.0895 ns | 0.6079 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
