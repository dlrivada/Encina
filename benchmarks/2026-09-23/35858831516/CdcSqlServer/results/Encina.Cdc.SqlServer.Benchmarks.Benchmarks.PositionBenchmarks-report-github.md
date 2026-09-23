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
| ComparePositions | 0.6772 ns | 1.5169 ns | 0.0831 ns |  0.15 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 4.6777 ns | 5.0579 ns | 0.2772 ns |  1.00 |    0.07 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.7857 ns | 2.2672 ns | 0.1243 ns |  1.24 |    0.07 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.7826 ns | 1.3156 ns | 0.0721 ns |  1.24 |    0.06 | 0.0004 |      32 B |        1.33 |
