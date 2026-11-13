```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|------------:|-----------:|-------:|----------:|
| ComparePositions |     1.637 ns |   0.4834 ns |  0.0265 ns |      - |         - |
| FromBytes        | 1,136.921 ns | 211.9776 ns | 11.6192 ns | 0.0439 |     744 B |
| ToBytes          |   484.456 ns | 166.5459 ns |  9.1289 ns | 0.0157 |     264 B |
