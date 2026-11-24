```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4987 ns | 0.6688 ns | 0.0367 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.8722 ns | 5.3639 ns | 0.2940 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.3268 ns | 9.0781 ns | 0.4976 ns |  1.12 |    0.13 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.0639 ns | 1.3391 ns | 0.0734 ns |  1.05 |    0.07 | 0.0019 |      32 B |        1.33 |
