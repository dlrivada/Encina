```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6900 ns |  0.0124 ns | 0.0007 ns |  0.11 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.0341 ns | 10.0143 ns | 0.5489 ns |  1.01 |    0.11 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.5369 ns |  0.6879 ns | 0.0377 ns |  1.26 |    0.10 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.6256 ns |  0.6484 ns | 0.0355 ns |  1.27 |    0.10 | 0.0004 |      32 B |        1.33 |
