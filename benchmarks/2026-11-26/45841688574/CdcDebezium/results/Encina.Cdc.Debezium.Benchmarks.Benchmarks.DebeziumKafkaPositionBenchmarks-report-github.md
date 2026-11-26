```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|----------:|----------:|-------:|----------:|
| ComparePositions |     2.880 ns |  5.724 ns | 0.3138 ns |      - |         - |
| FromBytes        | 1,468.370 ns | 35.407 ns | 1.9408 ns | 0.0439 |     744 B |
| ToBytes          |   677.041 ns | 24.582 ns | 1.3474 ns | 0.0153 |     264 B |
