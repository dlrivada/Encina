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
| CreateChangeEvent          | 23.534 ns | 11.5427 ns | 0.6327 ns |  0.65 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.402 ns |  0.4642 ns | 0.0254 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.358 ns |  1.7191 ns | 0.0942 ns |  0.37 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.353 ns |  2.9992 ns | 0.1644 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
