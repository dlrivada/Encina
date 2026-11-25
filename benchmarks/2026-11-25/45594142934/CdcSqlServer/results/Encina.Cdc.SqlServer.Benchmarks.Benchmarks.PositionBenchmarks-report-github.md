```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.266 ns |  0.0161 ns | 0.0009 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  7.264 ns |  0.4065 ns | 0.0223 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.485 ns | 12.1129 ns | 0.6639 ns |  1.86 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.456 ns |  2.2527 ns | 0.1235 ns |  1.03 |    0.01 | 0.0019 |      32 B |        1.33 |
