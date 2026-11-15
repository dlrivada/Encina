```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Median       | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------------:|-------:|----------:|
| ComparePositions |     2.689 ns |  0.0064 ns |  0.0087 ns |     2.687 ns |      - |         - |
| FromBytes        | 1,489.913 ns | 13.0638 ns | 17.8818 ns | 1,501.493 ns | 0.0439 |     744 B |
| ToBytes          |   742.726 ns |  6.7253 ns |  9.6452 ns |   746.274 ns | 0.0153 |     264 B |
