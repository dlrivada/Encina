```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Median       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.45 ns |   0.146 ns |   0.209 ns |     36.49 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     36.47 ns |   0.193 ns |   0.283 ns |     36.37 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,721.16 ns | 429.061 ns | 642.199 ns | 89,724.67 ns | 2,460.51 |   25.41 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    135.56 ns |   5.295 ns |   7.594 ns |    141.68 ns |     3.72 |    0.21 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.31 ns |   0.164 ns |   0.245 ns |     80.25 ns |     2.20 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
