```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Median       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.18 ns |   0.051 ns |   0.071 ns |     27.18 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.64 ns |   0.082 ns |   0.118 ns |     21.63 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 51,659.68 ns | 607.840 ns | 890.964 ns | 52,152.98 ns | 2,387.17 |   42.45 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     99.99 ns |   0.676 ns |   1.012 ns |     99.84 ns |     4.62 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.95 ns |   0.419 ns |   0.627 ns |     65.97 ns |     3.05 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
