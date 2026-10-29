```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.08 ns |     1.501 ns |   0.082 ns |     1.31 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.74 ns |     7.057 ns |   0.387 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,015.00 ns | 8,162.246 ns | 447.400 ns | 3,094.81 |   37.35 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.61 ns |    21.790 ns |   1.194 ns |     4.59 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.50 ns |    26.083 ns |   1.430 ns |     2.77 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
