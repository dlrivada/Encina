```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     44.94 ns |   0.742 ns |   1.087 ns |     45.72 ns |   1.33 |    0.03 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     33.83 ns |   0.067 ns |   0.100 ns |     33.84 ns |   1.00 |    0.00 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 32,975.12 ns | 270.104 ns | 387.374 ns | 32,967.13 ns | 974.75 |   11.60 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    138.29 ns |   2.458 ns |   3.526 ns |    135.39 ns |   4.09 |    0.10 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     96.98 ns |   0.137 ns |   0.197 ns |     97.01 ns |   2.87 |    0.01 | 0.0020 |     176 B |        1.57 |
