```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.43 ns |   0.074 ns |   0.108 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.98 ns |   0.072 ns |   0.105 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 64,341.02 ns | 235.967 ns | 345.877 ns | 2,299.75 |   14.81 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    128.23 ns |   0.550 ns |   0.824 ns |     4.58 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.09 ns |   0.323 ns |   0.483 ns |     2.97 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
