```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.49 ns | 12.966 ns | 0.711 ns | 11.17 |    0.06 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.22 ns |  0.752 ns | 0.041 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 594.22 ns | 58.011 ns | 3.180 ns | 48.63 |    0.27 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 579.54 ns | 38.906 ns | 2.133 ns | 47.43 |    0.21 | 0.0601 |    1008 B |       42.00 |
