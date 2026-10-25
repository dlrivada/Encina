```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.536 ns | 19.770 ns | 1.0837 ns | 15.32 |    0.80 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.737 ns |  9.547 ns | 0.5233 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 659.428 ns | 70.961 ns | 3.8896 ns | 75.66 |    3.95 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 587.370 ns | 76.220 ns | 4.1779 ns | 67.39 |    3.52 | 0.0601 |    1008 B |       42.00 |
