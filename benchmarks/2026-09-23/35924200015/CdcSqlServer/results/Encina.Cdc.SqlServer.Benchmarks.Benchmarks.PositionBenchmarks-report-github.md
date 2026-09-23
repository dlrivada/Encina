```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.8691 ns | 0.0506 ns | 0.0028 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 6.2156 ns | 0.6192 ns | 0.0339 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.0038 ns | 0.2925 ns | 0.0160 ns |  1.13 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 7.5359 ns | 0.8758 ns | 0.0480 ns |  1.21 | 0.0013 |      32 B |        1.33 |
