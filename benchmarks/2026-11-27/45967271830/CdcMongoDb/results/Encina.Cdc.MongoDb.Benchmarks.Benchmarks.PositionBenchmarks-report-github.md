```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.752 ns |  16.9611 ns | 0.9297 ns |  21.98 |    0.18 | 0.0038 |      96 B |        4.00 |
| CreatePosition   |   6.085 ns |   0.7150 ns | 0.0392 ns |   1.00 |    0.01 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 683.651 ns |  25.8793 ns | 1.4185 ns | 112.36 |    0.66 | 0.0401 |    1024 B |       42.67 |
| ToBytes          | 683.848 ns | 134.1879 ns | 7.3553 ns | 112.39 |    1.22 | 0.0401 |    1008 B |       42.00 |
