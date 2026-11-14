```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.333 ns | 0.2147 ns | 0.0118 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.833 ns | 1.1276 ns | 0.0618 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 14.945 ns | 8.6140 ns | 0.4722 ns |  2.19 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 19.304 ns | 2.3531 ns | 0.1290 ns |  2.83 |    0.03 | 0.0019 |      32 B |        1.33 |
