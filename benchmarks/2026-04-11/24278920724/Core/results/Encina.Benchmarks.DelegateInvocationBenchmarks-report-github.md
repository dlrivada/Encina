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
| CompiledDelegate        |     35.19 ns |   0.137 ns |   0.188 ns |     0.96 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     36.53 ns |   0.797 ns |   1.168 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 64,809.24 ns | 408.264 ns | 585.521 ns | 1,776.00 |   58.02 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    128.56 ns |   0.626 ns |   0.917 ns |     3.52 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.80 ns |   0.617 ns |   0.844 ns |     2.35 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
