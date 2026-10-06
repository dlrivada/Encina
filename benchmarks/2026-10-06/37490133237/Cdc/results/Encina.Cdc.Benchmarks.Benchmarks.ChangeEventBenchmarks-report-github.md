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
| CreateChangeEvent          | 35.130 ns | 41.9464 ns | 2.2992 ns |  0.83 |    0.05 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.500 ns |  0.3292 ns | 0.0180 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 18.831 ns |  7.8185 ns | 0.4286 ns |  0.44 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 42.524 ns |  5.3295 ns | 0.2921 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
