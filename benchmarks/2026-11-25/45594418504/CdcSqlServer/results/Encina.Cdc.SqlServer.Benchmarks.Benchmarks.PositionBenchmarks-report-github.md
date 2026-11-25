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
| ComparePositions | 0.4835 ns | 0.0717 ns | 0.0039 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.6264 ns | 1.8644 ns | 0.1022 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.9587 ns | 1.7101 ns | 0.0937 ns |  1.09 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.0352 ns | 0.2721 ns | 0.0149 ns |  1.11 |    0.03 | 0.0019 |      32 B |        1.33 |
