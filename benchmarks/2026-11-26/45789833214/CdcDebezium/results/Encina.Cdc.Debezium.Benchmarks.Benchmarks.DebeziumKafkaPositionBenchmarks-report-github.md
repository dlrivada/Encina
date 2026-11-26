```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|------------:|-----------:|-------:|----------:|
| ComparePositions |     3.469 ns |   0.0841 ns |  0.0046 ns |      - |         - |
| FromBytes        | 1,993.196 ns | 368.0722 ns | 20.1753 ns | 0.0420 |     744 B |
| ToBytes          |   911.465 ns |  76.9432 ns |  4.2175 ns | 0.0153 |     264 B |
