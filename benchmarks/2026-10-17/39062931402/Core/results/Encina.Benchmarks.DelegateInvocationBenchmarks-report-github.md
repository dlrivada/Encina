```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.28 ns |    16.182 ns |   0.887 ns |     1.30 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.61 ns |     9.879 ns |   0.542 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 16,043.83 ns | 2,950.211 ns | 161.711 ns | 1,028.49 |   32.61 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     66.14 ns |    45.940 ns |   2.518 ns |     4.24 |    0.19 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     43.41 ns |    18.398 ns |   1.008 ns |     2.78 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
