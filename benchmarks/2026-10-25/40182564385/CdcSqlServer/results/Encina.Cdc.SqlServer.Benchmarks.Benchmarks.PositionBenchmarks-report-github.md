```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5189 ns | 0.5366 ns | 0.0294 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5795 ns | 1.4811 ns | 0.0812 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8843 ns | 2.8027 ns | 0.1536 ns |  1.09 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3161 ns | 0.5272 ns | 0.0289 ns |  0.93 |    0.02 | 0.0019 |      32 B |        1.33 |
