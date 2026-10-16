```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.14 ns |     0.596 ns |   0.033 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.63 ns |     0.155 ns |   0.008 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,164.08 ns | 3,053.495 ns | 167.372 ns | 2,458.16 |    6.75 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    103.19 ns |     8.705 ns |   0.477 ns |     4.77 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     69.04 ns |    18.356 ns |   1.006 ns |     3.19 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
