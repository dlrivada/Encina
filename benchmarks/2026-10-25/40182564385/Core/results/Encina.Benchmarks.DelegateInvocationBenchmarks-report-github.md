```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     22.07 ns |     25.120 ns |   1.377 ns |     1.25 |    0.08 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     17.64 ns |     10.946 ns |   0.600 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 18,380.34 ns | 13,076.984 ns | 716.794 ns | 1,042.93 |   46.63 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     71.53 ns |     19.549 ns |   1.072 ns |     4.06 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     47.03 ns |      6.410 ns |   0.351 ns |     2.67 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
