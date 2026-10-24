```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.079 ns | 13.7359 ns | 0.7529 ns |  0.62 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.387 ns |  0.0293 ns | 0.0016 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.283 ns |  3.1452 ns | 0.1724 ns |  0.35 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.444 ns |  1.3307 ns | 0.0729 ns |  1.00 |    0.00 | 0.0033 |      56 B |        1.00 |
