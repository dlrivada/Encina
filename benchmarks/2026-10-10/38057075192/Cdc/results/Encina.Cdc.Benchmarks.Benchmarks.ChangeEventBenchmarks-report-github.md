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
| CreateChangeEvent          | 22.603 ns | 17.5550 ns | 0.9622 ns |  0.63 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.383 ns |  0.0450 ns | 0.0025 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.946 ns |  0.9647 ns | 0.0529 ns |  0.36 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.149 ns |  4.4491 ns | 0.2439 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
