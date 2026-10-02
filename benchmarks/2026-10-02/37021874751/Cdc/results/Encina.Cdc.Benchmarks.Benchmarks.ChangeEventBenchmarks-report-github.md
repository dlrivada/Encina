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
| CreateChangeEvent          | 23.156 ns | 14.0950 ns | 0.7726 ns |  0.63 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.255 ns |  0.0404 ns | 0.0022 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.341 ns |  3.0944 ns | 0.1696 ns |  0.36 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.634 ns |  0.6246 ns | 0.0342 ns |  1.00 |    0.00 | 0.0033 |      56 B |        1.00 |
