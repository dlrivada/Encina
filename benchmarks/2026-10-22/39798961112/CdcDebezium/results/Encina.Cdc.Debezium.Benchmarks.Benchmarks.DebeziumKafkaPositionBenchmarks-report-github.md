```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------:|----------:|
| ComparePositions |     2.558 ns |  0.0087 ns |  0.0119 ns |      - |         - |
| FromBytes        | 1,401.642 ns | 51.0931 ns | 71.6254 ns | 0.0076 |     744 B |
| ToBytes          |   558.079 ns | 16.4485 ns | 24.1100 ns | 0.0029 |     264 B |
