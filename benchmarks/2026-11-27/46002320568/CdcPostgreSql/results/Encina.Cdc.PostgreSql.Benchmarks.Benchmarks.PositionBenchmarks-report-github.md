```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4792 ns | 0.1066 ns | 0.0058 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.8708 ns | 6.6371 ns | 0.3638 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.1249 ns | 1.4744 ns | 0.0808 ns |  1.07 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.1661 ns | 1.0621 ns | 0.0582 ns |  1.08 |    0.09 | 0.0019 |      32 B |        1.33 |
