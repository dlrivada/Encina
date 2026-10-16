```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.019 ns | 13.9671 ns | 0.7656 ns |  0.69 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.398 ns |  0.2512 ns | 0.0138 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.763 ns |  2.2241 ns | 0.1219 ns |  0.38 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.035 ns |  2.7817 ns | 0.1525 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
