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
| CreateChangeEvent          | 23.270 ns | 19.8729 ns | 1.0893 ns |  0.64 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.389 ns |  0.0990 ns | 0.0054 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.323 ns |  2.1230 ns | 0.1164 ns |  0.37 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.238 ns | 11.2108 ns | 0.6145 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
