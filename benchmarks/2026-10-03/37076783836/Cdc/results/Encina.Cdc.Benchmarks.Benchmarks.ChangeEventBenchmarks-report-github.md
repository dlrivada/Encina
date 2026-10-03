```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.616 ns |  9.575 ns | 0.5249 ns |  0.70 |    0.03 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.408 ns |  1.437 ns | 0.0788 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.152 ns |  3.690 ns | 0.2022 ns |  0.33 |    0.02 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 30.750 ns | 27.771 ns | 1.5222 ns |  1.00 |    0.06 | 0.0007 |      56 B |        1.00 |
