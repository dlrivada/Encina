```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  74.231 ns |  14.9354 ns |  0.8187 ns | 16.08 |    0.19 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   4.615 ns |   0.6321 ns |  0.0346 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 344.050 ns | 272.3141 ns | 14.9265 ns | 74.55 |    2.84 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 319.245 ns |  28.1823 ns |  1.5448 ns | 69.17 |    0.54 | 0.0601 |    1008 B |       42.00 |
