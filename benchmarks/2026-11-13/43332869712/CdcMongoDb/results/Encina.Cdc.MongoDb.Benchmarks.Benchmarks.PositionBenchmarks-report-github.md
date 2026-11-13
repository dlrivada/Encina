```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 0.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 161.79 ns | 18.988 ns | 1.041 ns | 15.94 |    0.16 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |  10.15 ns |  1.845 ns | 0.101 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 651.21 ns | 35.388 ns | 1.940 ns | 64.14 |    0.58 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 600.20 ns | 12.477 ns | 0.684 ns | 59.12 |    0.51 | 0.0114 |    1008 B |       42.00 |
