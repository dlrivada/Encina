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
| CreateChangeEvent          | 21.037 ns | 13.3804 ns | 0.7334 ns |  0.60 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.381 ns |  0.0118 ns | 0.0006 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.257 ns |  6.0279 ns | 0.3304 ns |  0.35 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.238 ns | 13.0930 ns | 0.7177 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
