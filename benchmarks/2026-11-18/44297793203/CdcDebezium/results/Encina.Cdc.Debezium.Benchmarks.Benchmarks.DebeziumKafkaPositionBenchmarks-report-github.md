```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|------------:|----------:|-------:|----------:|
| ComparePositions |     3.545 ns |   0.5897 ns | 0.0323 ns |      - |         - |
| FromBytes        | 1,898.701 ns | 169.8563 ns | 9.3104 ns | 0.0439 |     744 B |
| ToBytes          |   891.858 ns |  71.0520 ns | 3.8946 ns | 0.0153 |     264 B |
