```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.04 ns |     1.551 ns |   0.085 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.15 ns |     1.506 ns |   0.083 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,683.84 ns | 8,544.914 ns | 468.376 ns | 2,333.68 |   15.58 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.62 ns |    24.640 ns |   1.351 ns |     4.78 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     87.19 ns |    26.129 ns |   1.432 ns |     3.10 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
