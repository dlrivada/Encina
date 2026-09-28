```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6381 ns | 0.1955 ns | 0.0107 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.2812 ns | 2.0407 ns | 0.1119 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.0526 ns | 1.1064 ns | 0.0606 ns |  1.18 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.1081 ns | 0.9305 ns | 0.0510 ns |  1.19 |    0.03 | 0.0004 |      32 B |        1.33 |
