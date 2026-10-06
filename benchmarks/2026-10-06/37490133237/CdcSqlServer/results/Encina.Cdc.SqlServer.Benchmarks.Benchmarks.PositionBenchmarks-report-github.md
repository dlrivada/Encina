```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.289 ns | 0.2821 ns | 0.0155 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.977 ns | 7.8739 ns | 0.4316 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.898 ns | 1.8128 ns | 0.0994 ns |  0.69 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.316 ns | 3.7682 ns | 0.2065 ns |  0.63 |    0.03 | 0.0019 |      32 B |        1.33 |
