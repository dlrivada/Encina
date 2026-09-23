```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9259 ns | 0.1100 ns | 0.0060 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.3542 ns | 1.5371 ns | 0.0843 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.1921 ns | 2.8967 ns | 0.1588 ns |  1.16 |    0.03 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 7.7199 ns | 3.8241 ns | 0.2096 ns |  1.44 |    0.04 | 0.0013 |      32 B |        1.33 |
